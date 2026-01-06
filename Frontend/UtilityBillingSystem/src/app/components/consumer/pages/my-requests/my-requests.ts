import { CommonModule } from '@angular/common';
import { Component, inject, signal, viewChild, effect } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../../services/auth/authService';
import { UtilityRequestService } from '../../../../services/core/utilityRequestService';
import { NotificationService } from '../../../../services/notification/notificationService';
import { Observable, Subject } from 'rxjs';
import { UtilityType } from '../../../../models/utility';
import { DisplayRequest } from '../../../../models/display/display-request';
import { CreateUtilityRequestDto } from '../../../../models/utility-request';

@Component({
  selector: 'app-my-requests',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatChipsModule,
    MatIconModule
  ],
  templateUrl: './my-requests.html',
  styleUrl: './my-requests.css',
})
export class MyRequests {
  private authService = inject(AuthService);
  private requestService = inject(UtilityRequestService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  sort = viewChild(MatSort);
  paginator = viewChild(MatPaginator);

  currentUser = this.authService.currentUser;
  isFormVisible = signal(false);
  
  displayedColumns: string[] = ['utilityName', 'requestDate', 'status', 'decisionDate'];
  dataSource = new MatTableDataSource<DisplayRequest>();
  
  private refreshTrigger$ = new Subject<void>();
  
  availableUtilities$: Observable<UtilityType[]> = this.requestService.getAvailableUtilitiesForUser(
    this.currentUser()!.id,
    this.refreshTrigger$
  );

  requestForm = this.fb.group({
    utilityTypeId: ['', Validators.required],
  });

  requests$: Observable<DisplayRequest[]> = this.requestService.getDisplayRequestsForUser(
    this.currentUser()!.id,
    this.refreshTrigger$
  );

  constructor() {
    this.requests$.subscribe(data => {
      this.dataSource.data = data;
    });

    effect(() => {
      if (this.sort()) {
        this.dataSource.sort = this.sort()!;
      }
      if (this.paginator()) {
        this.dataSource.paginator = this.paginator()!;
      }
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort()!;
    this.dataSource.paginator = this.paginator()!;
    
    this.dataSource.sortingDataAccessor = (item: DisplayRequest, property: string) => {
      switch (property) {
        case 'requestDate': return item.requestDate.getTime();
        case 'decisionDate': return item.decisionDate ? item.decisionDate.getTime() : 0;
        case 'utilityName': return item.utilityName.toLowerCase();
        case 'status': return item.status.toLowerCase();
        default: {
          const value = item[property as keyof DisplayRequest];
          if (value instanceof Date) {
            return value.getTime();
          }
          if (value === undefined || value === null) {
            return 0;
          }
          return typeof value === 'string' ? value.toLowerCase() : (value as string | number);
        }
      }
    };
  }

  submitRequest() {
    if (this.requestForm.invalid) return;

    const requestData: CreateUtilityRequestDto = {
      userId: this.currentUser()!.id,
      utilityTypeId: this.requestForm.value.utilityTypeId!,
    };
    
    this.requestService.createRequest(requestData).subscribe(() => {
      this.notificationService.show('Your request has been submitted. The admin will review it.');
      this.isFormVisible.set(false);
      this.requestForm.reset();
      this.refreshTrigger$.next();
    });
  }
}
