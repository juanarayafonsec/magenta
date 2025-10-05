import { 
  Box, 
  Checkbox, 
  FormControlLabel,
  IconButton,
  InputAdornment,
  Typography,
  Alert,
  CircularProgress
} from '@mui/material'
import { 
  Close as CloseIcon,
  Visibility,
  VisibilityOff
} from '@mui/icons-material'
import { useState } from 'react'
import { StyledDrawer, Header, Title, Subtitle, StyledTextField, SignUpButton, SignInLink } from './RegisterSidebar.styles'
import { registrationService } from '../services/registrationService'
import type { RegisterUserRequest } from '../types/registration'
import { useAuth } from '../contexts/AuthContext'


interface RegisterSidebarProps {
  open: boolean
  onClose: () => void
  onSwitchToSignIn: () => void
}

function RegisterSidebar({ open, onClose, onSwitchToSignIn }: RegisterSidebarProps) {
  const { login } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [ageVerified, setAgeVerified] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [submitSuccess, setSubmitSuccess] = useState('')
  const [formData, setFormData] = useState({
    email: '',
    username: '',
    password: '',
    confirmPassword: ''
  })
  const [errors, setErrors] = useState({
    email: '',
    username: '',
    password: '',
    confirmPassword: ''
  })

  const handlePasswordVisibility = () => {
    setShowPassword(!showPassword)
  }

  const validateEmail = (email: string) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
  }

  const validateUsername = (username: string) => {
    const usernameRegex = /^[a-zA-Z0-9_-]+$/
    return usernameRegex.test(username) && username.length >= 3 && username.length <= 50
  }

  const validatePassword = (password: string) => {
    return password.length >= 6 && password.length <= 100
  }

  const handleInputChange = (field: string, value: string) => {
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
    const newErrors = { email: '', username: '', password: '', confirmPassword: '' }
    let isValid = true

    // Validate email
    if (!formData.email) {
      newErrors.email = 'Email is required'
      isValid = false
    } else if (!validateEmail(formData.email)) {
      newErrors.email = 'Please enter a valid email address'
      isValid = false
    }

    // Validate username
    if (!formData.username) {
      newErrors.username = 'Username is required'
      isValid = false
    } else if (!validateUsername(formData.username)) {
      newErrors.username = 'Username must be 3-50 characters and contain only letters, numbers, hyphens, and underscores'
      isValid = false
    }

    // Validate password
    if (!formData.password) {
      newErrors.password = 'Password is required'
      isValid = false
    } else if (!validatePassword(formData.password)) {
      newErrors.password = 'Password must be between 6 and 100 characters'
      isValid = false
    }

    // Validate password confirmation
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = 'Please confirm your password'
      isValid = false
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match'
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

    if (!ageVerified) {
      setSubmitError('Please confirm that you are at least 18 years old')
      return
    }

    setIsLoading(true)

    try {
      const requestData: RegisterUserRequest = {
        username: formData.username,
        email: formData.email,
        password: formData.password,
        confirmPassword: formData.confirmPassword
      }

      const response = await registrationService.registerUser(requestData)

      if (response.success) {
        setSubmitSuccess('Account created successfully! Logging you in...')
        
        // Auto-login the user
        if (response.userId && response.username && response.email) {
          login({
            id: response.userId,
            username: response.username,
            email: response.email
          })
        }
        
        // Reset form
        setFormData({
          email: '',
          username: '',
          password: '',
          confirmPassword: ''
        })
        setAgeVerified(false)
        
        // Close the sidebar after a short delay
        setTimeout(() => {
          onClose()
        }, 1500)
      } else {
        setSubmitError(response.errors.join(', '))
      }
    } catch (error) {
      console.error('Registration error:', error)
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
          <Subtitle>Create your account. Instant registration</Subtitle>
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
          label="Email"
          placeholder="Enter your email"
          required
          value={formData.email}
          onChange={(e) => handleInputChange('email', e.target.value)}
          error={!!errors.email}
          helperText={errors.email}
          sx={{
            '& .MuiFormHelperText-root': {
              color: '#f44336',
            },
          }}
        />

        <StyledTextField
          fullWidth
          label="Username"
          placeholder="Enter your username"
          required
          value={formData.username}
          onChange={(e) => handleInputChange('username', e.target.value)}
          error={!!errors.username}
          helperText={errors.username}
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

        <StyledTextField
          fullWidth
          label="Confirm Password"
          type={showPassword ? 'text' : 'password'}
          placeholder="Confirm your password"
          required
          value={formData.confirmPassword}
          onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
          error={!!errors.confirmPassword}
          helperText={errors.confirmPassword}
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


        <FormControlLabel
          control={
            <Checkbox
              checked={ageVerified}
              onChange={(e) => setAgeVerified(e.target.checked)}
              sx={{
                color: '#6A1B9A',
                '&.Mui-checked': {
                  color: '#6A1B9A',
                },
              }}
            />
          }
          label={
            <Typography sx={{ color: 'rgba(255, 255, 255, 0.7)', fontSize: '0.9rem' }}>
              By signing up I attest that I am at least 18 years old and have read the{' '}
              <span className="link">Terms of Service</span>
            </Typography>
          }
        />


        <SignUpButton 
          onClick={handleSubmit}
          disabled={isLoading}
          startIcon={isLoading ? <CircularProgress size={20} color="inherit" /> : null}
        >
          {isLoading ? 'Creating Account...' : 'Sign Up & Play'}
        </SignUpButton>

        <SignInLink>
          Do you already have an account?{' '}
          <span className="link" onClick={onSwitchToSignIn}>Click here to Sign In</span>
        </SignInLink>
      </Box>
    </StyledDrawer>
  )
}

export default RegisterSidebar
