export interface Notification {
  id: string;
  userId: string;
  billId?: string;
  type: string; 
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

