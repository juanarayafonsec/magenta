import { TextField, type TextFieldProps } from "@mui/material";

interface TextInputProps extends Omit<TextFieldProps, 'error' | 'helperText'> {
  error?: string;
  helperText?: string;
}

export default function TextInput({ error, helperText, ...props }: TextInputProps) {
  return (
    <TextField
      {...props}
      fullWidth
      variant="outlined"
      error={!!error}
      helperText={error || helperText}
    />
  );
}
