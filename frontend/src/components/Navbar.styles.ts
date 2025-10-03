import { AppBar, Typography, Button } from '@mui/material'
import { styled } from '@mui/material/styles'

export const StyledAppBar = styled(AppBar)(() => ({
  backgroundColor: '#1A1A2E',
  boxShadow: 'none',
  zIndex: 1200,
  marginLeft: 0,
  width: '100%',
  position: 'absolute',
  top: 0,
  left: 0,
}))

export const Logo = styled(Typography)(() => ({
  fontWeight: 'bold',
  fontSize: '1.5rem',
  color: '#fff',
  whiteSpace: 'nowrap',
  overflow: 'visible',
  textOverflow: 'unset',
}))

export const SignUpButton = styled(Button)(() => ({
  backgroundColor: '#6A1B9A',
  color: '#fff',
  textTransform: 'none',
  fontWeight: 600,
  padding: '8px 24px',
  '&:hover': {
    backgroundColor: '#7B2CBF',
  },
}))
