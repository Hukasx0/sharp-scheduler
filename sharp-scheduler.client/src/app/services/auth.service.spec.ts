import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should login and store token', () => {
    const mockResponse = { token: 'fakeToken' };
    spyOn(localStorage, 'setItem');

    service.login('username', 'password').subscribe();

    const req = httpMock.expectOne('/api/Account/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);

    expect(localStorage.setItem).toHaveBeenCalledWith('token', 'fakeToken');
  });

  it('should logout and clear token', () => {
    spyOn(localStorage, 'removeItem');

    service.logout();

    expect(localStorage.removeItem).toHaveBeenCalledWith('token');
  });
});
