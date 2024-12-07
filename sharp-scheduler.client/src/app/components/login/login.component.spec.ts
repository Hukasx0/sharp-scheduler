import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FormBuilder } from '@angular/forms';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['login']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      declarations: [ LoginComponent ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
        FormBuilder
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should submit the login form successfully', () => {
    const mockResponse = { token: 'fakeToken' };
    mockAuthService.login.and.returnValue(of(mockResponse));

    component.loginForm.setValue({ username: 'user', password: 'pass' });
    component.onSubmit();

    expect(mockAuthService.login).toHaveBeenCalledWith('user', 'pass');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should show error message on failed login', () => {
    mockAuthService.login.and.returnValue(throwError(() => new Error('Invalid credentials')));

    component.loginForm.setValue({ username: 'user', password: 'wrong' });
    component.onSubmit();

    expect(component.errorMessage).toBe('Invalid username or password. Please try again.');
  });
});
