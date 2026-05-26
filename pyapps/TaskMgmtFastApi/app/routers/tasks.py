"""Task CRUD routes (scoped to the authenticated user)."""
from datetime import datetime, timezone

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.database import get_db
from app.models import User, UserTask
from app.schemas import CreateTaskRequest, TaskResponse, UpdateTaskRequest
from app.security import get_current_user

router = APIRouter(prefix="/api/Tasks", tags=["Tasks"])


def _get_owned_task(db: Session, task_id: int, user: User) -> UserTask:
    task = (
        db.query(UserTask)
        .filter(UserTask.task_id == task_id, UserTask.user_id == user.user_id)
        .first()
    )
    if task is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Task not found")
    return task


@router.get("", response_model=list[TaskResponse])
def list_tasks(
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> list[UserTask]:
    return (
        db.query(UserTask)
        .filter(UserTask.user_id == user.user_id)
        .order_by(UserTask.task_id.desc())
        .all()
    )


@router.post("", response_model=TaskResponse, status_code=status.HTTP_201_CREATED)
def create_task(
    payload: CreateTaskRequest,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> UserTask:
    task = UserTask(
        user_id=user.user_id,
        title=payload.title,
        due_date=payload.due_date,
        is_completed=False,
        created_at=datetime.now(timezone.utc),
    )
    db.add(task)
    db.commit()
    db.refresh(task)
    return task


@router.get("/{id}", response_model=TaskResponse)
def get_task(
    id: int,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> UserTask:
    return _get_owned_task(db, id, user)


@router.put("/{id}", response_model=TaskResponse)
def update_task(
    id: int,
    payload: UpdateTaskRequest,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> UserTask:
    task = _get_owned_task(db, id, user)
    task.title = payload.title
    task.is_completed = payload.is_completed
    task.due_date = payload.due_date
    db.commit()
    db.refresh(task)
    return task


@router.delete("/{id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_task(
    id: int,
    db: Session = Depends(get_db),
    user: User = Depends(get_current_user),
) -> None:
    task = _get_owned_task(db, id, user)
    db.delete(task)
    db.commit()
