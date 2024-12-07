import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http'; // Interceptor related imports
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  // Intercept HTTP requests and add an Authorization header if a token is available
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = localStorage.getItem('token'); // Get token from localStorage
    if (token) {
      // Clone the request and add the Authorization header with the token
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request); // Continue with the request
  }
}
