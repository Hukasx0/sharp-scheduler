import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router'; // Router for navigation
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {
  constructor(private router: Router) {}

  // Method to check if the user is authenticated before activating a route
  canActivate(): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    const token = localStorage.getItem('token'); // Check if a token is present in localStorage

    if (!token) {
      console.log('No token found, redirecting to login');
      this.router.navigate(['/login']); // Redirect to login if no token is found
      return false;
    }

    return true; // Allow navigation if the token exists
  }
}
