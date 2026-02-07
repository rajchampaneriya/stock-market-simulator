import { createTheme, alpha } from '@mui/material/styles';

// TradingView-inspired color palette
const palette = {
  bg: {
    primary: '#131722',
    secondary: '#1e222d',
    tertiary: '#2a2e39',
    elevated: '#363a45',
    hover: '#3c4049',
  },
  text: {
    primary: '#d1d4dc',
    secondary: '#787b86',
    tertiary: '#5d606b',
    accent: '#ffffff',
  },
  bullish: {
    main: '#26a69a',
    light: '#4db6ac',
    dark: '#00897b',
    bg: 'rgba(38, 166, 154, 0.12)',
  },
  bearish: {
    main: '#ef5350',
    light: '#e57373',
    dark: '#c62828',
    bg: 'rgba(239, 83, 80, 0.12)',
  },
  accent: {
    blue: '#2962ff',
    blueLight: '#5b8def',
    purple: '#7c4dff',
    orange: '#ff9800',
    cyan: '#00bcd4',
  },
  border: {
    main: '#2a2e39',
    light: '#363a45',
    focus: '#2962ff',
  },
};

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: palette.accent.blue,
      light: palette.accent.blueLight,
    },
    secondary: {
      main: palette.accent.purple,
    },
    success: {
      main: palette.bullish.main,
      light: palette.bullish.light,
      dark: palette.bullish.dark,
    },
    error: {
      main: palette.bearish.main,
      light: palette.bearish.light,
      dark: palette.bearish.dark,
    },
    warning: {
      main: palette.accent.orange,
    },
    info: {
      main: palette.accent.cyan,
    },
    background: {
      default: palette.bg.primary,
      paper: palette.bg.secondary,
    },
    text: {
      primary: palette.text.primary,
      secondary: palette.text.secondary,
    },
    divider: palette.border.main,
  },
  typography: {
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    h1: {
      fontSize: '1.75rem',
      fontWeight: 700,
      letterSpacing: '-0.02em',
      color: palette.text.accent,
    },
    h2: {
      fontSize: '1.5rem',
      fontWeight: 700,
      letterSpacing: '-0.01em',
      color: palette.text.accent,
    },
    h3: {
      fontSize: '1.25rem',
      fontWeight: 600,
      color: palette.text.accent,
    },
    h4: {
      fontSize: '1.1rem',
      fontWeight: 600,
      color: palette.text.primary,
    },
    h5: {
      fontSize: '1rem',
      fontWeight: 600,
      color: palette.text.primary,
    },
    h6: {
      fontSize: '0.875rem',
      fontWeight: 600,
      color: palette.text.primary,
    },
    subtitle1: {
      fontSize: '0.8125rem',
      color: palette.text.secondary,
      fontWeight: 400,
    },
    subtitle2: {
      fontSize: '0.75rem',
      color: palette.text.secondary,
      fontWeight: 500,
      textTransform: 'uppercase' as const,
      letterSpacing: '0.05em',
    },
    body1: {
      fontSize: '0.8125rem',
      color: palette.text.primary,
    },
    body2: {
      fontSize: '0.75rem',
      color: palette.text.secondary,
    },
    caption: {
      fontSize: '0.6875rem',
      color: palette.text.tertiary,
    },
  },
  shape: {
    borderRadius: 4,
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: palette.bg.primary,
          scrollbarWidth: 'thin',
          scrollbarColor: `${palette.bg.elevated} ${palette.bg.primary}`,
          '&::-webkit-scrollbar': {
            width: '8px',
            height: '8px',
          },
          '&::-webkit-scrollbar-track': {
            background: palette.bg.primary,
          },
          '&::-webkit-scrollbar-thumb': {
            background: palette.bg.elevated,
            borderRadius: '4px',
          },
          '&::-webkit-scrollbar-thumb:hover': {
            background: palette.bg.hover,
          },
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: palette.bg.secondary,
          backgroundImage: 'none',
          borderBottom: `1px solid ${palette.border.main}`,
          boxShadow: 'none',
        },
      },
    },
    MuiToolbar: {
      styleOverrides: {
        root: {
          minHeight: '48px !important',
          paddingLeft: '16px !important',
          paddingRight: '16px !important',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundColor: palette.bg.secondary,
          backgroundImage: 'none',
          border: `1px solid ${palette.border.main}`,
          boxShadow: 'none',
          '&:hover': {
            borderColor: palette.border.light,
          },
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundColor: palette.bg.secondary,
          backgroundImage: 'none',
          border: `1px solid ${palette.border.main}`,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
          fontSize: '0.6875rem',
          height: '22px',
          borderRadius: '4px',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          fontSize: '0.8125rem',
          borderRadius: '4px',
          padding: '4px 12px',
          minHeight: '32px',
        },
        outlined: {
          borderColor: palette.border.light,
          '&:hover': {
            borderColor: palette.accent.blue,
            backgroundColor: alpha(palette.accent.blue, 0.08),
          },
        },
        contained: {
          boxShadow: 'none',
          '&:hover': {
            boxShadow: 'none',
          },
        },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 500,
          fontSize: '0.8125rem',
          minHeight: '40px',
          padding: '8px 16px',
          color: palette.text.secondary,
          '&.Mui-selected': {
            color: palette.text.accent,
          },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderBottomColor: palette.border.main,
          fontSize: '0.8125rem',
          padding: '8px 12px',
        },
        head: {
          fontWeight: 600,
          color: palette.text.secondary,
          textTransform: 'uppercase' as const,
          fontSize: '0.6875rem',
          letterSpacing: '0.05em',
          backgroundColor: palette.bg.tertiary,
        },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        root: {
          '&:hover': {
            backgroundColor: `${alpha(palette.accent.blue, 0.04)} !important`,
          },
        },
      },
    },
    MuiAlert: {
      styleOverrides: {
        root: {
          borderRadius: '4px',
          fontSize: '0.8125rem',
        },
      },
    },
    MuiAccordion: {
      styleOverrides: {
        root: {
          backgroundColor: palette.bg.tertiary,
          border: `1px solid ${palette.border.main}`,
          '&:before': {
            display: 'none',
          },
          '&.Mui-expanded': {
            margin: '8px 0',
          },
        },
      },
    },
    MuiAccordionSummary: {
      styleOverrides: {
        root: {
          minHeight: '40px',
          '&.Mui-expanded': {
            minHeight: '40px',
          },
        },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: {
          color: palette.text.secondary,
          '&:hover': {
            backgroundColor: alpha(palette.text.primary, 0.05),
          },
        },
      },
    },
    MuiTooltip: {
      styleOverrides: {
        tooltip: {
          backgroundColor: palette.bg.elevated,
          border: `1px solid ${palette.border.light}`,
          fontSize: '0.75rem',
          fontWeight: 400,
        },
      },
    },
  },
});

export { palette };
export default theme;
