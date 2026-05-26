"""SQLAlchemy ORM models mapped to existing LEARNING database tables."""
from datetime import datetime
from typing import List, Optional

from sqlalchemy import Boolean, DateTime, ForeignKey, Integer, LargeBinary, String
from sqlalchemy.orm import Mapped, mapped_column, relationship

from app.database import Base


class User(Base):
    __tablename__ = "Users"

    user_id: Mapped[int] = mapped_column("UserId", Integer, primary_key=True, autoincrement=True)
    first_name: Mapped[Optional[str]] = mapped_column("FirstName", String(100), nullable=True)
    last_name: Mapped[Optional[str]] = mapped_column("LastName", String(100), nullable=True)
    email: Mapped[str] = mapped_column("Email", String(255), nullable=False, unique=True)
    phone: Mapped[Optional[str]] = mapped_column("Phone", String(50), nullable=True)
    password_hash: Mapped[Optional[bytes]] = mapped_column("PasswordHash", LargeBinary, nullable=True)
    is_active: Mapped[Optional[bool]] = mapped_column("IsActive", Boolean, nullable=True)
    created_date: Mapped[Optional[datetime]] = mapped_column("CreatedDate", DateTime, nullable=True)

    tasks: Mapped[List["UserTask"]] = relationship(back_populates="user")


class UserTask(Base):
    __tablename__ = "Tasks"

    task_id: Mapped[int] = mapped_column("TaskId", Integer, primary_key=True, autoincrement=True)
    user_id: Mapped[int] = mapped_column("UserId", Integer, ForeignKey("Users.UserId"), nullable=False)
    title: Mapped[str] = mapped_column("Title", String(150), nullable=False)
    is_completed: Mapped[Optional[bool]] = mapped_column("IsCompleted", Boolean, nullable=True)
    due_date: Mapped[Optional[datetime]] = mapped_column("DueDate", DateTime, nullable=True)
    created_at: Mapped[Optional[datetime]] = mapped_column("CreatedAt", DateTime, nullable=True)

    user: Mapped["User"] = relationship(back_populates="tasks")
