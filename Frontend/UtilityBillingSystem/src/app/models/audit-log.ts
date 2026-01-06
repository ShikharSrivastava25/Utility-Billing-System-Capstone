export interface AuditLog {
  timestamp: Date;
  action: string;
  details: string;
  performedBy: string; 
}

