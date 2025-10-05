# Registration Form Integration

This document describes the integration between the frontend registration form and the Registration API.

## Features Implemented

### 1. Axios Interceptor Setup
- **File**: `frontend/src/services/api.ts`
- **Purpose**: Centralized HTTP client configuration with request/response interceptors
- **Features**:
  - Base URL configuration for Registration API
  - Request/response logging
  - Error handling
  - Timeout configuration (10 seconds)

### 2. Registration Service
- **File**: `frontend/src/services/registrationService.ts`
- **Purpose**: Service layer for registration API calls
- **Features**:
  - Type-safe request/response interfaces
  - Error handling and transformation
  - Network error handling

### 3. Enhanced Form Validation
- **File**: `frontend/src/components/RegisterSidebar.tsx`
- **Features**:
  - **Email Validation**: Validates email format using regex
  - **Username Validation**: 3-50 characters, alphanumeric with hyphens/underscores
  - **Password Validation**: 6-100 characters
  - **Password Confirmation**: Ensures passwords match
  - **Real-time Validation**: Errors clear as user types
  - **Age Verification**: Requires checkbox confirmation

### 4. User Experience Enhancements
- **Loading States**: Button shows spinner during API calls
- **Success/Error Messages**: Clear feedback using Material-UI Alerts
- **Form Reset**: Automatically clears form on successful registration
- **Auto-close**: Sidebar closes after successful registration (2-second delay)

## API Integration Details

### Endpoint
- **URL**: `http://localhost:5238/api/auth/register`
- **Method**: POST
- **Content-Type**: application/json

### Request Format
```typescript
{
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
}
```

### Response Format
```typescript
{
  success: boolean;
  userId?: string;
  username?: string;
  email?: string;
  errors: string[];
}
```

## Validation Rules

### Frontend Validation
- **Email**: Must be valid email format
- **Username**: 3-50 characters, only letters, numbers, hyphens, underscores
- **Password**: 6-100 characters
- **Confirm Password**: Must match password
- **Age Verification**: Must be checked

### Backend Validation (from API)
- Same rules as frontend plus additional server-side validation
- Username uniqueness check
- Email uniqueness check
- Additional security validations

## Error Handling

1. **Client-side Validation**: Shows field-specific error messages
2. **Network Errors**: Shows generic network error message
3. **Server Errors**: Displays server-provided error messages
4. **Success**: Shows success message and resets form

## Usage

The registration form is accessible through the main application sidebar. Users can:

1. Fill out the registration form
2. See real-time validation feedback
3. Submit the form (with loading state)
4. Receive success/error feedback
5. Form automatically resets on success

## Development Notes

- The API base URL is configured in `frontend/src/services/api.ts`
- CORS is configured in the Registration API to allow all origins
- The integration uses TypeScript for type safety
- All API calls are logged for debugging purposes
