import { Drawer, Box, Typography, TextField, Button, FormControlLabel } from '@mui/material'
import { styled } from '@mui/material/styles'

export const StyledDrawer = styled(Drawer)(() => ({
  '& .MuiDrawer-paper': {
    backgroundColor: '#1A1A2E',
    width: 400,
    overflow: 'auto',
    display: 'flex',
    flexDirection: 'column',
    zIndex: 1500,
    position: 'fixed',
    top: 0,
    right: 0,
    height: '100vh',
    boxShadow: '-2px 0 8px rgba(0, 0, 0, 0.3)',
  },
}))

export const Header = styled(Box)(() => ({
  padding: '24px',
  borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
}))

export const Title = styled(Typography)(() => ({
  fontWeight: 'bold',
  fontSize: '1.5rem',
  color: '#fff',
}))

export const Subtitle = styled(Typography)(() => ({
  color: 'rgba(255, 255, 255, 0.7)',
  fontSize: '0.9rem',
  marginTop: '8px',
}))

export const StyledTextField = styled(TextField)(() => ({
  marginBottom: '16px',
  '& .MuiOutlinedInput-root': {
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    borderRadius: '8px',
    '& fieldset': {
      borderColor: 'rgba(255, 255, 255, 0.2)',
    },
    '&:hover fieldset': {
      borderColor: 'rgba(255, 255, 255, 0.3)',
    },
    '&.Mui-focused fieldset': {
      borderColor: '#6A1B9A',
    },
  },
  '& .MuiInputLabel-root': {
    color: 'rgba(255, 255, 255, 0.7)',
  },
  '& .MuiOutlinedInput-input': {
    color: '#fff',
  },
}))

export const SignUpButton = styled(Button)(() => ({
  backgroundColor: '#6A1B9A',
  color: '#fff',
  textTransform: 'none',
  fontWeight: 600,
  padding: '12px 16px',
  borderRadius: '8px',
  width: '100%',
  marginTop: '16px',
  '&:hover': {
    backgroundColor: '#7B2CBF',
  },
}))

export const SignInLink = styled(Typography)(() => ({
  color: 'rgba(255, 255, 255, 0.7)',
  textAlign: 'center',
  marginTop: '16px',
  '& .link': {
    color: '#6A1B9A',
    cursor: 'pointer',
    textDecoration: 'underline',
    '&:hover': {
      color: '#7B2CBF',
    },
  },
}))
