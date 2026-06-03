import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TaskService } from '../core/task.service';
import { AuthService } from '../core/auth.service';
import { TaskItem } from '../core/models';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.scss'
})
export class TasksComponent {
  private fb = inject(FormBuilder);
  private taskService = inject(TaskService);
  private auth = inject(AuthService);
  private router = inject(Router);

  tasks = signal<TaskItem[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  editingId = signal<number | null>(null);

  createForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(150)]],
    dueDate: ['']
  });

  editForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(150)]],
    isCompleted: [false],
    dueDate: ['']
  });

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.taskService.list().subscribe({
      next: items => {
        this.tasks.set(items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message || 'Failed to load tasks.');
        this.loading.set(false);
      }
    });
  }

  create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const v = this.createForm.getRawValue();
    this.taskService.create({
      title: v.title.trim(),
      dueDate: v.dueDate ? new Date(v.dueDate).toISOString() : null
    }).subscribe({
      next: () => {
        this.createForm.reset({ title: '', dueDate: '' });
        this.refresh();
      },
      error: err => this.error.set(err?.error?.message || 'Failed to create task.')
    });
  }

  startEdit(task: TaskItem): void {
    this.editingId.set(task.id);
    this.editForm.setValue({
      title: task.title,
      isCompleted: !!task.isCompleted,
      dueDate: task.dueDate ? task.dueDate.substring(0, 10) : ''
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: number): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    const v = this.editForm.getRawValue();
    this.taskService.update(id, {
      title: v.title.trim(),
      isCompleted: v.isCompleted,
      dueDate: v.dueDate ? new Date(v.dueDate).toISOString() : null
    }).subscribe({
      next: () => {
        this.editingId.set(null);
        this.refresh();
      },
      error: err => this.error.set(err?.error?.message || 'Failed to update task.')
    });
  }

  toggleComplete(task: TaskItem): void {
    this.taskService.update(task.id, {
      title: task.title,
      isCompleted: !task.isCompleted,
      dueDate: task.dueDate ?? null
    }).subscribe({
      next: () => this.refresh(),
      error: err => this.error.set(err?.error?.message || 'Failed to update task.')
    });
  }

  remove(task: TaskItem): void {
    if (!confirm(`Delete task "${task.title}"?`)) return;
    this.taskService.delete(task.id).subscribe({
      next: () => this.refresh(),
      error: err => this.error.set(err?.error?.message || 'Failed to delete task.')
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
