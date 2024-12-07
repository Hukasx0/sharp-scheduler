import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeComponent } from './home.component';
import { AuthService } from '../../services/auth.service';
import { JobService } from '../../services/job.service';
import { Router } from '@angular/router';
import { FormBuilder } from '@angular/forms';
import { of } from 'rxjs';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockJobService: jasmine.SpyObj<JobService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['logout', 'currentUser']);
    mockJobService = jasmine.createSpyObj('JobService', ['getJobs', 'addJob', 'deleteJob', 'updateJobStatus']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      declarations: [ HomeComponent ],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: JobService, useValue: mockJobService },
        { provide: Router, useValue: mockRouter },
        FormBuilder
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load jobs on init', () => {
    const mockJobs = { jobs: [], totalPages: 1, currentPage: 1 };
    mockJobService.getJobs.and.returnValue(of(mockJobs));

    component.ngOnInit();

    expect(mockJobService.getJobs).toHaveBeenCalled();
    expect(component.jobs).toEqual(mockJobs.jobs);
  });

  it('should add a new job', () => {
    const newJob = {
      id: 1,
      name: 'Test Job',
      command: 'echo',
      cronExpression: '* * * * *',
      isActive: true,
      createdAt: new Date().toISOString(),
      lastExecution: new Date().toISOString()
    };

    mockJobService.addJob.and.returnValue(of(newJob));

    component.jobForm.setValue({
      name: 'Test Job',
      command: 'echo',
      cronExpression: '* * * * *',
      isActive: true
    });

    component.addJob();

    expect(mockJobService.addJob).toHaveBeenCalledWith(newJob);
    expect(component.jobs).toContain(newJob);
  });
});
