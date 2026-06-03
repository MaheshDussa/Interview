export interface LoginRequest {
  email: string;
}

export interface LoginResponse {
  token?: string;
  accessToken?: string;
  jwt?: string;
  [key: string]: any;
}

export interface CreateTaskRequest {
  title: string;
  dueDate?: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  isCompleted: boolean;
  dueDate?: string | null;
}

export interface TaskItem {
  id: number;
  title: string;
  isCompleted: boolean;
  dueDate?: string | null;
}
