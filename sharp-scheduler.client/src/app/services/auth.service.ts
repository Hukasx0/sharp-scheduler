import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, Observable, throwError, tap } from 'rxjs';
import { User } from '../interfaces/user';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null); // Store current user

  constructor(private http: HttpClient) { }

  // Login method to authenticate user and store token
  login(username: string, password: string): Observable<any> {
    return this.http.post('/api/Account/login', { username, password }).pipe(
      tap((response: any) => {
        if (response.token) {
          localStorage.setItem('token', response.token); // Store token in localStorage
        }
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

  // Logout method to clear user session and token
  logout() {
    localStorage.removeItem('token'); // Remove token from localStorage
    this.currentUserSubject.next(null); // Clear current user
  }

  // Getter to return current user observable
  get currentUser(): Observable<User | null> {
    return this.currentUserSubject.asObservable();
  }

  // Getter to check if user is logged in
  get isLoggedIn(): boolean {
    return !!this.currentUserSubject.value;
  }
}
