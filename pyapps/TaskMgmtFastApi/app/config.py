"""Application configuration."""
from urllib.parse import quote_plus


class Settings:
    # SQL Server connection (Windows Authentication)
    ODBC_CONNECTION_STRING = (
        "Driver={ODBC Driver 17 for SQL Server};"
        "Server=localhost\\SQLEXPRESS01;"
        "Database=LEARNING;"
        "Trusted_Connection=yes;"
        "Encrypt=no;"
        "TrustServerCertificate=no;"
    )

    @property
    def database_url(self) -> str:
        return f"mssql+pyodbc:///?odbc_connect={quote_plus(self.ODBC_CONNECTION_STRING)}"

    # JWT
    JWT_SECRET = "change-this-secret-in-production-please-use-env-var"
    JWT_ALGORITHM = "HS256"
    JWT_EXPIRE_MINUTES = 60

    API_TITLE = "Task Management API"
    API_DESCRIPTION = "API for task management with email-based authentication"
    API_VERSION = "v1"


settings = Settings()
