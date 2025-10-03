import { 
  Box, 
  Checkbox, 
  FormControlLabel,
  IconButton,
  InputAdornment,
  Typography
} from '@mui/material'
import { 
  Close as CloseIcon,
  Visibility,
  VisibilityOff
} from '@mui/icons-material'
import { useState } from 'react'
import { StyledDrawer, Header, Title, Subtitle, StyledTextField, SignUpButton, SignInLink } from './RegisterSidebar.styles'


interface RegisterSidebarProps {
  open: boolean
  onClose: () => void
  onSwitchToSignIn: () => void
}

function RegisterSidebar({ open, onClose, onSwitchToSignIn }: RegisterSidebarProps) {
  const [showPassword, setShowPassword] = useState(false)
  const [ageVerified, setAgeVerified] = useState(false)
  const [formData, setFormData] = useState({
    email: '',
    username: '',
    password: '',
    confirmPassword: ''
  })
  const [errors, setErrors] = useState({
    email: '',
    password: ''
  })

  const handlePasswordVisibility = () => {
    setShowPassword(!showPassword)
  }

  const validateEmail = (email: string) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
  }

  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    
    // Clear errors when user starts typing
    if (errors[field as keyof typeof errors]) {
      setErrors(prev => ({ ...prev, [field]: '' }))
    }
  }

  const validateForm = () => {
    const newErrors = { email: '', password: '' }
    let isValid = true

    // Validate email
    if (!formData.email) {
      newErrors.email = 'Email is required'
      isValid = false
    } else if (!validateEmail(formData.email)) {
      newErrors.email = 'Please enter a valid email address'
      isValid = false
    }

    // Validate password match
    if (formData.password !== formData.confirmPassword) {
      newErrors.password = 'Passwords do not match'
      isValid = false
    }

    setErrors(newErrors)
    return isValid
  }

  const handleSubmit = () => {
    if (validateForm() && ageVerified) {
      // Handle form submission
      console.log('Form submitted:', formData)
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
        />

        <StyledTextField
          fullWidth
          label="Password"
          type={showPassword ? 'text' : 'password'}
          placeholder="Enter your password"
          required
          value={formData.password}
          onChange={(e) => handleInputChange('password', e.target.value)}
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


        <SignUpButton onClick={handleSubmit}>
          Sign Up & Play
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
