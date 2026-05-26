"""Authentication routes."""
from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from app.database import get_db
from app.models import User
from app.schemas import LoginRequest, TokenResponse
from app.security import create_access_token

router = APIRouter(prefix="/api/Auth", tags=["Auth"])


@router.post("/login", response_model=TokenResponse)
def login(payload: LoginRequest, db: Session = Depends(get_db)) -> TokenResponse:
    """Email-based login. Issues a JWT for an active user with the given email."""
    user = db.query(User).filter(User.email == payload.email).first()
    if user is None or user.is_active is False:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid email or inactive user",
        )

    token, expires_in = create_access_token(user)
    return TokenResponse(access_token=token, expires_in=expires_in)
