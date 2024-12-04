import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, Observable, tap, throwError } from 'rxjs';
import { User } from '../interfaces/user';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);

  constructor(private http: HttpClient) { }

   login(username: string, password: string): Observable<any> {
    return this.http.post('/api/Account/login', { username, password }).pipe(
      tap((response: any) => {
        console.log('Login response:', response);
        if (response.token) {
          console.log('Saving token:', response.token);
          localStorage.setItem('token', response.token);
        } else {
          console.error('No token in response');
        }
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

   logout() {
     localStorage.removeItem('token');
     this.currentUserSubject.next(null);
   }


   get currentUser(): Observable<User | null> {
     return this.currentUserSubject.asObservable();
   }

   get isLoggedIn(): boolean {
     return !!this.currentUserSubject.value;
   }
}
