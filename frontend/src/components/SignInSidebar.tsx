import { 
  Box, 
  Button,
  IconButton,
  InputAdornment,
  Alert,
  CircularProgress
} from '@mui/material'
import { 
  Close as CloseIcon,
  Visibility,
  VisibilityOff
} from '@mui/icons-material'
import { useState } from 'react'
import { StyledDrawer, Header, Title, Subtitle, StyledTextField, SignInButton, SignUpLink } from './SignInSidebar.styles'
import { authenticationService } from '../services/authenticationService'
import { useAuth } from '../contexts/AuthContext'
import type { LoginRequest } from '../types/authentication'


interface SignInSidebarProps {
  open: boolean
  onClose: () => void
  onSwitchToSignUp: () => void
}

function SignInSidebar({ open, onClose, onSwitchToSignUp }: SignInSidebarProps) {
  const { login } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [submitSuccess, setSubmitSuccess] = useState('')
  const [formData, setFormData] = useState({
    usernameOrEmail: '',
    password: ''
  })
  const [errors, setErrors] = useState({
    usernameOrEmail: '',
    password: ''
  })

  const handlePasswordVisibility = () => {
    setShowPassword(!showPassword)
  }

  const handleInputChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    
    // Clear errors when user starts typing
    if (errors[field as keyof typeof errors]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
    
    // Clear submit messages when user starts typing
    if (submitError) setSubmitError('')
    if (submitSuccess) setSubmitSuccess('')
  }

  const validateForm = () => {
    const newErrors = { usernameOrEmail: '', password: '' }
    let isValid = true

    // Validate username or email
    if (!formData.usernameOrEmail) {
      newErrors.usernameOrEmail = 'Username or email is required'
      isValid = false
    }

    // Validate password
    if (!formData.password) {
      newErrors.password = 'Password is required'
      isValid = false
    }

    setErrors(newErrors)
    return isValid
  }

  const handleSubmit = async () => {
    // Clear previous messages
    setSubmitError('')
    setSubmitSuccess('')

    if (!validateForm()) {
      return
    }

    setIsLoading(true)

    try {
      const requestData: LoginRequest = {
        usernameOrEmail: formData.usernameOrEmail,
        password: formData.password,
        rememberMe: false
      }

      const response = await authenticationService.login(requestData)

      if (response.success && response.user) {
        setSubmitSuccess('Login successful! Welcome back.')
        
        // Login the user
        login({
          id: '', // We don't have ID from login response
          username: response.user.username,
          email: response.user.email
        })
        
        // Reset form
        setFormData({
          usernameOrEmail: '',
          password: ''
        })
        
        // Close the sidebar after a short delay
        setTimeout(() => {
          onClose()
        }, 1500)
      } else {
        setSubmitError(response.errors.join(', '))
      }
    } catch (error) {
      console.error('Login error:', error)
      setSubmitError('An unexpected error occurred. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <StyledDrawer
      anchor="right"
      open={open}
      onClose={onClose}
    >
      <Header>
        <Box>
          <Title variant="h5">Magenta</Title>
          <Subtitle>Welcome back! Sign in to your account</Subtitle>
        </Box>
        <IconButton onClick={onClose} sx={{ color: '#fff' }}>
          <CloseIcon />
        </IconButton>
      </Header>

      <Box sx={{ p: 3, flexGrow: 1 }}>
        {/* Success/Error Messages */}
        {submitSuccess && (
          <Alert severity="success" sx={{ mb: 2 }}>
            {submitSuccess}
          </Alert>
        )}
        
        {submitError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {submitError}
          </Alert>
        )}

        <StyledTextField
          fullWidth
          label="Email or Username"
          placeholder="Enter your email or username"
          required
          value={formData.usernameOrEmail}
          onChange={(e) => handleInputChange('usernameOrEmail', e.target.value)}
          error={!!errors.usernameOrEmail}
          helperText={errors.usernameOrEmail}
          sx={{
            '& .MuiFormHelperText-root': {
              color: '#f44336',
            },
          }}
        />

        <StyledTextField
          fullWidth
          label="Password"
          type={showPassword ? 'text' : 'password'}
          placeholder="Enter your password"
          required
          value={formData.password}
          onChange={(e) => handleInputChange('password', e.target.value)}
          error={!!errors.password}
          helperText={errors.password}
          sx={{
            '& .MuiFormHelperText-root': {
              color: '#f44336',
            },
          }}
          InputProps={{
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  onClick={handlePasswordVisibility}
                  edge="end"
                  sx={{ color: 'rgba(255, 255, 255, 0.7)' }}
                >
                  {showPassword ? <VisibilityOff /> : <Visibility />}
                </IconButton>
              </InputAdornment>
            ),
          }}
        />


        <Button
          sx={{
            color: '#6A1B9A',
            textTransform: 'none',
            fontWeight: 500,
            fontSize: '0.9rem',
            justifyContent: 'flex-start',
            paddingLeft: 0,
            '&:hover': {
              backgroundColor: 'transparent',
              textDecoration: 'underline',
            },
          }}
        >
          Forgot password?
        </Button>

        <SignInButton 
          onClick={handleSubmit}
          disabled={isLoading}
          startIcon={isLoading ? <CircularProgress size={20} color="inherit" /> : null}
        >
          {isLoading ? 'Signing In...' : 'Sign In'}
        </SignInButton>

        <SignUpLink>
          Don't have an account?{' '}
          <span className="link" onClick={onSwitchToSignUp}>Sign up here</span>
        </SignUpLink>
      </Box>
    </StyledDrawer>
  )
}

export default SignInSidebar
