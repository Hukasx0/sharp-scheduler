import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms'; // Angular FormBuilder for creating the login form
import { AuthService } from '../../services/auth.service'; // AuthService to handle authentication logic
import { Router } from '@angular/router'; // Router for navigation

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm = this.fb.group({
    username: ['', [Validators.required]], // Username field with required validation
    password: ['', [Validators.required]] // Password field with required validation
  });

  errorMessage: string = ''; // Error message to display if login fails

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {}

  // Handle form submission
  onSubmit() {
    if (this.loginForm.valid) {
      const { username, password } = this.loginForm.value;
      this.authService.login(username!, password!).subscribe({
        next: () => {
          this.loginForm.reset(); // Reset form on successful login
          this.router.navigate(['/']); // Navigate to home page
        },
        error: (error) => {
          this.errorMessage = 'Invalid username or password. Please try again.'; // Show error message
          this.loginForm.reset(); // Reset form on error
          console.error('Login error:', error); // Log error to console
        }
      });
    }
  }
}
