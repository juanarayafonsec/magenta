import { useState } from 'react'
import { Box, ThemeProvider, CssBaseline } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { theme } from './theme/theme'
import Navbar from './components/Navbar'
import Sidebar from './components/Sidebar'
import { AuthProvider } from './contexts/AuthContext'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error: any) => {
        // Don't retry on 401 errors (unauthorized)
        if (error?.response?.status === 401) {
          return false;
        }
        // Retry up to 2 times for other errors
        return failureCount < 2;
      },
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
})

function App() {
  const [count, setCount] = useState(0)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <AuthProvider>
        <Box sx={{ 
          backgroundColor: 'background.default', 
          minHeight: '100vh',
          width: '100vw',
          margin: 0,
          padding: 0,
          overflowX: 'hidden',
          position: 'relative'
        }}>
        <Sidebar onToggle={setSidebarCollapsed} />
        <Navbar sidebarCollapsed={sidebarCollapsed} />
        <Box sx={{ 
          marginLeft: sidebarCollapsed ? '60px' : '280px',
          marginTop: '64px',
          minHeight: 'calc(100vh - 64px)',
          position: 'relative',
          transition: 'margin-left 0.3s ease',
          backgroundColor: 'background.default',
          width: sidebarCollapsed ? 'calc(100% - 60px)' : 'calc(100% - 280px)'
        }}>
          <Box sx={{ 
            p: { xs: 2, sm: 4 },
            width: '100%',
            maxWidth: '100%',
            margin: 0,
            overflowX: 'hidden',
            flexGrow: 1,
            backgroundColor: 'background.default'
          }}>
            <div>
              <a href="https://vite.dev" target="_blank">
                <img src={viteLogo} className="logo" alt="Vite logo" />
              </a>
              <a href="https://react.dev" target="_blank">
                <img src={reactLogo} className="logo react" alt="React logo" />
              </a>
            </div>
            <h1 style={{ color: 'text.primary' }}>Vite + React</h1>
            <div className="card">
              <button onClick={() => setCount((count) => count + 1)}>
                count is {count}
              </button>
              <p style={{ color: 'text.primary' }}>
                Edit <code>src/App.tsx</code> and save to test HMR
              </p>
            </div>
            <p className="read-the-docs" style={{ color: 'text.primary' }}>
              Click on the Vite and React logos to learn more
            </p>
          </Box>
        </Box>
      </Box>
    </AuthProvider>
    </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App
