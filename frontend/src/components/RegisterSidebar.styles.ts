import { Drawer, Box, Typography, TextField, Button, FormControlLabel } from '@mui/material'
import { styled } from '@mui/material/styles'

export const StyledDrawer = styled(Drawer)(({ theme }) => ({
  '& .MuiDrawer-paper': {
    backgroundColor: theme.palette.background.paper,
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

export const Header = styled(Box)(({ theme }) => ({
  padding: '24px',
  borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
}))

export const Title = styled(Typography)(({ theme }) => ({
  fontWeight: 'bold',
  fontSize: '1.5rem',
  color: theme.palette.text.primary,
}))

export const Subtitle = styled(Typography)(({ theme }) => ({
  color: theme.palette.text.secondary,
  fontSize: '0.9rem',
  marginTop: '8px',
}))

export const StyledTextField = styled(TextField)(({ theme }) => ({
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
      borderColor: theme.palette.primary.main,
    },
  },
  '& .MuiInputLabel-root': {
    color: theme.palette.text.secondary,
  },
  '& .MuiOutlinedInput-input': {
    color: theme.palette.text.primary,
  },
}))

export const SignUpButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  textTransform: 'none',
  fontWeight: 600,
  padding: '12px 16px',
  borderRadius: '8px',
  width: '100%',
  marginTop: '16px',
  '&:hover': {
    backgroundColor: theme.palette.primary.light,
  },
}))

export const SignInLink = styled(Typography)(({ theme }) => ({
  color: theme.palette.text.secondary,
  textAlign: 'center',
  marginTop: '16px',
  '& .link': {
    color: theme.palette.primary.main,
    cursor: 'pointer',
    textDecoration: 'underline',
    '&:hover': {
      color: theme.palette.primary.light,
    },
  },
}))
