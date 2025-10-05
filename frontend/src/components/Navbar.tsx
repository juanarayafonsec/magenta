import { Toolbar, Button, Box, useMediaQuery, useTheme, Typography, IconButton, Menu, MenuItem } from '@mui/material'
import { useState } from 'react'
import { AccountCircle } from '@mui/icons-material'
import RegisterSidebar from './RegisterSidebar'
import SignInSidebar from './SignInSidebar'
import { StyledAppBar, Logo, SignUpButton } from './Navbar.styles'
import { useAuth } from '../contexts/AuthContext'

interface NavbarProps {
  sidebarCollapsed: boolean
}

function Navbar({ sidebarCollapsed }: NavbarProps) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const { user, isAuthenticated, logout } = useAuth()
  const [registerSidebarOpen, setRegisterSidebarOpen] = useState(false)
  const [signInSidebarOpen, setSignInSidebarOpen] = useState(false)
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)

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

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget)
  }

  const handleProfileMenuClose = () => {
    setAnchorEl(null)
  }

  const handleLogout = () => {
    logout()
    handleProfileMenuClose()
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
            {!isAuthenticated ? (
              <>
                <Button 
                  onClick={handleSignInClick}
                  sx={{ 
                    color: 'text.primary',
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
              </>
            ) : (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="body2" sx={{ color: 'text.primary' }}>
                  Welcome, {user?.username}
                </Typography>
                <IconButton
                  size="large"
                  edge="end"
                  aria-label="account of current user"
                  aria-controls="profile-menu"
                  aria-haspopup="true"
                  onClick={handleProfileMenuOpen}
                  color="inherit"
                >
                  <AccountCircle />
                </IconButton>
                <Menu
                  id="profile-menu"
                  anchorEl={anchorEl}
                  anchorOrigin={{
                    vertical: 'bottom',
                    horizontal: 'right',
                  }}
                  keepMounted
                  transformOrigin={{
                    vertical: 'top',
                    horizontal: 'right',
                  }}
                  open={Boolean(anchorEl)}
                  onClose={handleProfileMenuClose}
                >
                  <MenuItem onClick={handleLogout}>Logout</MenuItem>
                </Menu>
              </Box>
            )}
          </Box>
        )}

        {/* Mobile Menu Button - Simplified for auth only */}
        {isMobile && (
          <Box sx={{ flexGrow: 1, display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
            {!isAuthenticated ? (
              <>
                <Button 
                  onClick={handleSignInClick}
                  sx={{ 
                    color: 'text.primary',
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
                    backgroundColor: 'primary.main',
                    color: 'primary.contrastText',
                    textTransform: 'none',
                    fontWeight: 600,
                    padding: '8px 16px',
                    '&:hover': {
                      backgroundColor: 'primary.light',
                    }
                  }}
                >
                  Sign up
                </Button>
              </>
            ) : (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="body2" sx={{ color: 'text.primary', fontSize: '0.8rem' }}>
                  {user?.username}
                </Typography>
                <IconButton
                  size="small"
                  edge="end"
                  aria-label="account of current user"
                  aria-controls="profile-menu"
                  aria-haspopup="true"
                  onClick={handleProfileMenuOpen}
                  color="inherit"
                >
                  <AccountCircle />
                </IconButton>
                <Menu
                  id="profile-menu"
                  anchorEl={anchorEl}
                  anchorOrigin={{
                    vertical: 'bottom',
                    horizontal: 'right',
                  }}
                  keepMounted
                  transformOrigin={{
                    vertical: 'top',
                    horizontal: 'right',
                  }}
                  open={Boolean(anchorEl)}
                  onClose={handleProfileMenuClose}
                >
                  <MenuItem onClick={handleLogout}>Logout</MenuItem>
                </Menu>
              </Box>
            )}
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
