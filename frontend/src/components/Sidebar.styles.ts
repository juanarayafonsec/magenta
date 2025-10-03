import { Drawer, Typography, Button, ListItemButton } from '@mui/material'
import { styled } from '@mui/material/styles'

export const StyledDrawer = styled(Drawer)(() => ({
  '& .MuiDrawer-paper': {
    backgroundColor: '#1A1A2E',
    width: 280,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    zIndex: 1400,
    position: 'fixed',
    top: 0,
    left: 0,
    height: '100vh',
    boxShadow: '2px 0 8px rgba(0, 0, 0, 0.3)',
  },
}))

export const Logo = styled(Typography)(() => ({
  fontWeight: 'bold',
  fontSize: '1.5rem',
  color: '#fff',
  padding: '16px',
  textAlign: 'center',
  borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
}))

export const CollapseButton = styled(Button)(() => ({
  minWidth: 'auto',
  padding: '8px',
  color: '#fff',
  '&:hover': {
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
  },
}))

export const DownloadButton = styled(Button)(() => ({
  backgroundColor: '#6A1B9A',
  color: '#fff',
  textTransform: 'none',
  fontWeight: 600,
  padding: '12px 16px',
  margin: '16px',
  borderRadius: '8px',
  '&:hover': {
    backgroundColor: '#7B2CBF',
  },
}))

export const ActiveItem = styled(ListItemButton)(() => ({
  backgroundColor: 'rgba(106, 27, 154, 0.2)',
  borderLeft: '3px solid #6A1B9A',
  color: '#fff !important',
  '&:hover': {
    backgroundColor: 'rgba(106, 27, 154, 0.3)',
  },
  '& .MuiListItemText-primary': {
    color: '#fff !important',
  },
}))

export const InactiveItem = styled(ListItemButton)(() => ({
  color: '#fff',
  '&:hover': {
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
  },
  '& .MuiListItemText-primary': {
    color: '#fff !important',
  },
}))
