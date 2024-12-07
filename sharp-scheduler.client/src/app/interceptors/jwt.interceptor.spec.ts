import { TestBed } from '@angular/core/testing';
import { JwtInterceptor } from './jwt.interceptor';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { HttpRequest } from '@angular/common/http';

describe('JwtInterceptor', () => {
  let interceptor: JwtInterceptor;
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [JwtInterceptor]
    });

    interceptor = TestBed.inject(JwtInterceptor);
    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  it('should add Authorization header if token is available', () => {
    // Mocking localStorage to return a fake token
    spyOn(localStorage, 'getItem').and.returnValue('fakeToken');

    // Making an HTTP request
    httpClient.get('/api/Auth').subscribe();

    // Expect the request to be made to /api/Auth
    const httpRequest = httpMock.expectOne('/api/Auth');
    expect(httpRequest.request.headers.has('Authorization')).toBeTrue();
    expect(httpRequest.request.headers.get('Authorization')).toBe('Bearer fakeToken');

    // Responding to the HTTP request
    httpRequest.flush({});
  });

  it('should not add Authorization header if no token is available', () => {
    // Mocking localStorage to return null (no token)
    spyOn(localStorage, 'getItem').and.returnValue(null);

    // Making an HTTP request
    httpClient.get('/api/Auth').subscribe();

    // Expect the request to be made to /api/Auth
    const httpRequest = httpMock.expectOne('/api/Auth');
    expect(httpRequest.request.headers.has('Authorization')).toBeFalse();

    // Responding to the HTTP request
    httpRequest.flush({});
  });

  afterEach(() => {
    // Ensure that there are no outstanding HTTP requests after each test
    httpMock.verify();
  });
});
