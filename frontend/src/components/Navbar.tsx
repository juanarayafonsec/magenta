import { Toolbar, Button, Box, useMediaQuery, useTheme } from '@mui/material'
import { useState } from 'react'
import RegisterSidebar from './RegisterSidebar'
import SignInSidebar from './SignInSidebar'
import { StyledAppBar, Logo, SignUpButton } from './Navbar.styles'

interface NavbarProps {
  sidebarCollapsed: boolean
}

function Navbar({ sidebarCollapsed }: NavbarProps) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const [registerSidebarOpen, setRegisterSidebarOpen] = useState(false)
  const [signInSidebarOpen, setSignInSidebarOpen] = useState(false)

  const handleRegisterClick = () => {
    setRegisterSidebarOpen(true)
  }

  const handleRegisterClose = () => {
    setRegisterSidebarOpen(false)
  }

  const handleSignInClick = () => {
    setSignInSidebarOpen(true)
  }

  const handleSignInClose = () => {
    setSignInSidebarOpen(false)
  }

  const handleSwitchToSignUp = () => {
    setSignInSidebarOpen(false)
    setRegisterSidebarOpen(true)
  }

  const handleSwitchToSignIn = () => {
    setRegisterSidebarOpen(false)
    setSignInSidebarOpen(true)
  }


  return (
    <StyledAppBar 
      position="static" 
      sx={{ 
        marginLeft: sidebarCollapsed ? '60px' : '280px', 
        width: sidebarCollapsed ? 'calc(100% - 60px)' : 'calc(100% - 280px)',
        transition: 'margin-left 0.3s ease, width 0.3s ease',
      }}
    >
      <Toolbar sx={{ minHeight: '64px !important' }}>
        {sidebarCollapsed && (
          <Logo variant="h6" sx={{ marginRight: 2 }}>
            Magenta
          </Logo>
        )}
        
        {/* Desktop Navigation - Removed to avoid duplication with sidebar */}
        {!isMobile && (
          <Box sx={{ flexGrow: 1, display: 'flex', alignItems: 'center' }}>
            {/* Navigation items moved to sidebar */}
          </Box>
        )}
        
        {/* Desktop Auth Buttons */}
        {!isMobile && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Button 
              onClick={handleSignInClick}
              sx={{ 
                color: '#fff',
                textTransform: 'none',
                '&:hover': {
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                }
              }}
            >
              Log in
            </Button>
            <SignUpButton variant="contained" onClick={handleRegisterClick}>
              Sign up
            </SignUpButton>
          </Box>
        )}

        {/* Mobile Menu Button - Simplified for auth only */}
        {isMobile && (
          <Box sx={{ flexGrow: 1, display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
            <Button 
              onClick={handleSignInClick}
              sx={{ 
                color: '#fff',
                textTransform: 'none',
                '&:hover': {
                  backgroundColor: 'rgba(255, 255, 255, 0.1)',
                }
              }}
            >
              Log in
            </Button>
            <Button
              onClick={handleRegisterClick}
              sx={{
                backgroundColor: '#6A1B9A',
                color: '#fff',
                textTransform: 'none',
                fontWeight: 600,
                padding: '8px 16px',
                '&:hover': {
                  backgroundColor: '#7B2CBF',
                }
              }}
            >
              Sign up
            </Button>
          </Box>
        )}
      </Toolbar>
      <RegisterSidebar 
        open={registerSidebarOpen} 
        onClose={handleRegisterClose}
        onSwitchToSignIn={handleSwitchToSignIn}
      />
      <SignInSidebar 
        open={signInSidebarOpen} 
        onClose={handleSignInClose}
        onSwitchToSignUp={handleSwitchToSignUp}
      />
    </StyledAppBar>
  )
}

export default Navbar
