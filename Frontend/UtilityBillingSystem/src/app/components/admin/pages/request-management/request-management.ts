import { CommonModule } from '@angular/common';
import { Component, inject, signal, ViewChild, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RequestApprovalForm } from '../../forms/request-approval-form/request-approval-form';
import { UtilityRequestService } from '../../../../services/core/utilityRequestService';
import { UserService } from '../../../../services/core/userService';
import { UtilityService } from '../../../../services/core/utilityService';
import { TariffService } from '../../../../services/core/tariffService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { DisplayRequest } from '../../../../models/display/display-request';
import { TariffPlan } from '../../../../models/tariff';
import { combineLatest, debounceTime, map, Observable, startWith } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog';

@Component({
  selector: 'app-request-management',
  imports: [
    CommonModule, 
    MatProgressSpinnerModule, 
    MatTableModule, 
    MatSortModule, 
    MatPaginatorModule, 
    MatButtonModule, 
    MatChipsModule, 
    RequestApprovalForm, 
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    ReactiveFormsModule
  ],
  templateUrl: './request-management.html',
  styleUrl: './request-management.css',
})
export class RequestManagement implements OnInit {
  private requestService = inject(UtilityRequestService);
  private userService = inject(UserService);
  private utilityService = inject(UtilityService);
  private tariffService = inject(TariffService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);

  displayedColumns: string[] = ['consumerName', 'userId', 'utilityName', 'requestDate', 'status', 'actions'];
  dataSource = new MatTableDataSource<DisplayRequest>([]);
  searchControl = new FormControl('');
  
  @ViewChild(MatSort) set sort(sort: MatSort) {
    this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator) {
    this.dataSource.paginator = paginator;
  }
  
  isApprovalFormVisible = signal(false);
  selectedRequest = signal<DisplayRequest | null>(null);
  isLoading = signal(false);

  activeTariffs$: Observable<TariffPlan[]> = this.tariffService.getTariffs().pipe(
    map(tariffs => tariffs.filter(t => t.isActive))
  );

  constructor() {
    this.dataSource.sortingDataAccessor = (item: DisplayRequest, property: string) => {
      const value = item[property as keyof DisplayRequest];
      if (typeof value === 'string') {
        const dateValue = new Date(value);
        if (!isNaN(dateValue.getTime())) {
          return dateValue.getTime();
        }
        return value.toLowerCase();
      }
      if (value instanceof Date) {
        return value.getTime();
      }
      return (value ?? '') as string | number;
    };

    this.dataSource.filterPredicate = (data: DisplayRequest, filter: string) => {
      const searchTerm = filter.toLowerCase();
      return data.consumerName.toLowerCase().includes(searchTerm) ||
             data.userId.toLowerCase().includes(searchTerm) ||
             data.utilityName.toLowerCase().includes(searchTerm) ||
             data.status.toLowerCase().includes(searchTerm) ||
             new Date(data.requestDate).toDateString().toLowerCase().includes(searchTerm) ||
             new Date(data.requestDate).toLocaleDateString().toLowerCase().includes(searchTerm);
    };
  }

  ngOnInit() {
    this.loadRequests();

    this.searchControl.valueChanges.pipe(
      debounceTime(200),
      startWith('')
    ).subscribe(value => {
      this.dataSource.filter = value?.trim().toLowerCase() || '';
    });
  }

  loadRequests() {
    this.isLoading.set(true);
    combineLatest([
      this.requestService.getRequests(),
      this.userService.getUsers(),
      this.utilityService.getUtilities(),
    ]).pipe(
      map(([requests, users, utilities]) => {
        const userMap = new Map(users.map(u => [u.id, u.name]));
        const utilityMap = new Map(utilities.map(u => [u.id, u.name]));
        return requests
          .map(req => {
            const parseDate = (dateStr: string | Date): Date => {
              if (dateStr instanceof Date) return dateStr;
              const str = typeof dateStr === 'string' ? dateStr : String(dateStr);
              if (!str.includes('Z') && !str.match(/[+-]\d{2}:?\d{2}$/)) {
                return new Date(str + 'Z');
              }
              return new Date(str);
            };
            
            return {
              ...req,
              requestDate: parseDate(req.requestDate),
              decisionDate: req.decisionDate ? parseDate(req.decisionDate) : undefined,
              consumerName: userMap.get(req.userId) ?? 'Unknown User',
              utilityName: utilityMap.get(req.utilityTypeId) ?? 'Unknown Utility',
            };
          })
          .sort((a,b) => b.requestDate.getTime() - a.requestDate.getTime());
      })
    ).subscribe({
      next: (requests) => {
        this.dataSource.data = requests;
        if (this.dataSource.sort) {
          this.dataSource.sort.sort({ id: 'requestDate', start: 'desc', disableClear: false });
        }
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading requests:', error);
        this.dataSource.data = [];
        this.isLoading.set(false);
      }
    });
  }

  showApprovalForm(request: DisplayRequest) {
    this.selectedRequest.set(request);
    this.isApprovalFormVisible.set(true);
  }

  hideForms() {
    this.isApprovalFormVisible.set(false);
    this.selectedRequest.set(null);
  }

  handleApproval(payload: { tariffId: string, meterNumber: string }) {
    const request = this.selectedRequest();
    if (!request) return;
    
    this.requestService.approveRequest(request, payload).subscribe(() => {
      this.notificationService.show(`Request for ${request.utilityName} approved for ${request.consumerName}.`);
      this.hideForms();
      this.loadRequests();
    });
  }

  handleRejection(request: DisplayRequest) {
    this.dialog.open(ConfirmationDialogComponent).afterClosed().subscribe(result => {
      if (result) {
        this.requestService.rejectRequest(request.id).subscribe(() => {
          this.notificationService.show(`Request for ${request.utilityName} by ${request.consumerName} has been rejected.`, 'error');
          this.loadRequests();
        });
      }
    });
  }
}
