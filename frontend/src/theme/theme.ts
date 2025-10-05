import { createTheme } from '@mui/material/styles';
import { colors } from './colors';

/**
 * Material-UI theme configuration for the Magenta application
 * This theme provides consistent styling across all components
 */

const themeOptions = {
  palette: {
    mode: 'dark',
    primary: {
      main: colors.primary.main,
      light: colors.primary.light,
      dark: colors.primary.dark,
      contrastText: colors.primary.contrastText,
    },
    secondary: {
      main: colors.secondary.main,
      light: colors.secondary.light,
      dark: colors.secondary.dark,
      contrastText: colors.secondary.contrastText,
    },
    background: {
      default: colors.background.default,
      paper: colors.background.paper,
    },
    text: {
      primary: colors.text.primary,
      secondary: colors.text.secondary,
      disabled: colors.text.disabled,
    },
    error: {
      main: colors.error.main,
      light: colors.error.light,
      dark: colors.error.dark,
    },
    warning: {
      main: colors.warning.main,
      light: colors.warning.light,
      dark: colors.warning.dark,
    },
    info: {
      main: colors.info.main,
      light: colors.info.light,
      dark: colors.info.dark,
    },
    success: {
      main: colors.success.main,
      light: colors.success.light,
      dark: colors.success.dark,
    },
  },
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    h1: {
      fontSize: '2.5rem',
      fontWeight: 600,
      color: colors.text.primary,
    },
    h2: {
      fontSize: '2rem',
      fontWeight: 600,
      color: colors.text.primary,
    },
    h3: {
      fontSize: '1.75rem',
      fontWeight: 500,
      color: colors.text.primary,
    },
    h4: {
      fontSize: '1.5rem',
      fontWeight: 500,
      color: colors.text.primary,
    },
    h5: {
      fontSize: '1.25rem',
      fontWeight: 500,
      color: colors.text.primary,
    },
    h6: {
      fontSize: '1rem',
      fontWeight: 500,
      color: colors.text.primary,
    },
    body1: {
      fontSize: '1rem',
      color: colors.text.primary,
    },
    body2: {
      fontSize: '0.875rem',
      color: colors.text.secondary,
    },
    button: {
      textTransform: 'none',
      fontWeight: 600,
    },
  },
  components: {
    // Global component overrides
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: colors.background.default,
          color: colors.text.primary,
        },
      },
    },
    // Button component customization
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: '8px',
          textTransform: 'none',
          fontWeight: 600,
          padding: '8px 16px',
        },
        contained: {
          boxShadow: 'none',
          '&:hover': {
            boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)',
          },
        },
      },
    },
    // TextField component customization
    MuiTextField: {
      styleOverrides: {
        root: {
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
              borderColor: colors.primary.main,
            },
          },
          '& .MuiInputLabel-root': {
            color: colors.text.secondary,
          },
          '& .MuiOutlinedInput-input': {
            color: colors.text.primary,
          },
        },
      },
    },
    // AppBar component customization
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: colors.background.paper,
          boxShadow: 'none',
        },
      },
    },
    // Drawer component customization
    MuiDrawer: {
      styleOverrides: {
        paper: {
          backgroundColor: colors.background.paper,
        },
      },
    },
    // ListItem component customization
    MuiListItem: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: 'rgba(255, 255, 255, 0.05)',
          },
        },
      },
    },
    // Typography component customization
    MuiTypography: {
      styleOverrides: {
        root: {
          '&.MuiTypography-h1': {
            color: colors.text.primary,
          },
          '&.MuiTypography-h2': {
            color: colors.text.primary,
          },
          '&.MuiTypography-h3': {
            color: colors.text.primary,
          },
          '&.MuiTypography-h4': {
            color: colors.text.primary,
          },
          '&.MuiTypography-h5': {
            color: colors.text.primary,
          },
          '&.MuiTypography-h6': {
            color: colors.text.primary,
          },
          '&.MuiTypography-body1': {
            color: colors.text.primary,
          },
          '&.MuiTypography-body2': {
            color: colors.text.secondary,
          },
        },
      },
    },
  },
  // Custom theme properties
  custom: {
    colors: colors,
    gradients: colors.gradients,
    shadows: colors.shadows,
    borders: colors.border,
  },
};

// Extend the theme type to include custom properties
declare module '@mui/material/styles' {
  interface Theme {
    custom: {
      colors: typeof colors;
      gradients: typeof colors.gradients;
      shadows: typeof colors.shadows;
      borders: typeof colors.border;
    };
  }

  interface ThemeOptions {
    custom?: {
      colors?: typeof colors;
      gradients?: typeof colors.gradients;
      shadows?: typeof colors.shadows;
      borders?: typeof colors.border;
    };
  }
}

// Create and export the theme
export const theme = createTheme(themeOptions);

// Export theme options for testing or additional configuration
export { themeOptions };
