import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateTaskRequest, TaskItem, UpdateTaskRequest } from './models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl.replace(/\/+$/, '')}/api/Tasks`;

  list(): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(this.url);
  }

  get(id: number): Observable<TaskItem> {
    return this.http.get<TaskItem>(`${this.url}/${id}`);
  }

  create(payload: CreateTaskRequest): Observable<TaskItem> {
    return this.http.post<TaskItem>(this.url, payload);
  }

  update(id: number, payload: UpdateTaskRequest): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${this.url}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
