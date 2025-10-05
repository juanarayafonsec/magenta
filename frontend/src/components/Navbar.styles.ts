import { AppBar, Typography, Button } from '@mui/material'
import { styled } from '@mui/material/styles'

export const StyledAppBar = styled(AppBar)(({ theme }) => ({
  backgroundColor: theme.palette.background.paper,
  boxShadow: 'none',
  zIndex: 1200,
  marginLeft: 0,
  width: '100%',
  position: 'absolute',
  top: 0,
  left: 0,
}))

export const Logo = styled(Typography)(({ theme }) => ({
  fontWeight: 'bold',
  fontSize: '1.5rem',
  color: theme.palette.text.primary,
  whiteSpace: 'nowrap',
  overflow: 'visible',
  textOverflow: 'unset',
}))

export const SignUpButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  textTransform: 'none',
  fontWeight: 600,
  padding: '8px 24px',
  '&:hover': {
    backgroundColor: theme.palette.primary.light,
  },
}))
