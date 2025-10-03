import { 
  Box, 
  Button,
  IconButton,
  InputAdornment
} from '@mui/material'
import { 
  Close as CloseIcon,
  Visibility,
  VisibilityOff
} from '@mui/icons-material'
import { useState } from 'react'
import { StyledDrawer, Header, Title, Subtitle, StyledTextField, SignInButton, SignUpLink } from './SignInSidebar.styles'


interface SignInSidebarProps {
  open: boolean
  onClose: () => void
  onSwitchToSignUp: () => void
}

function SignInSidebar({ open, onClose, onSwitchToSignUp }: SignInSidebarProps) {
  const [showPassword, setShowPassword] = useState(false)

  const handlePasswordVisibility = () => {
    setShowPassword(!showPassword)
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
        <StyledTextField
          fullWidth
          label="Email or Username"
          placeholder="Enter your email or username"
          required
        />

        <StyledTextField
          fullWidth
          label="Password"
          type={showPassword ? 'text' : 'password'}
          placeholder="Enter your password"
          required
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

        <SignInButton>
          Sign In
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
