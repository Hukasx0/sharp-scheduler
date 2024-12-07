import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LogsComponent } from './logs.component';
import { JobService } from '../../services/job.service';
import { of } from 'rxjs';

describe('LogsComponent', () => {
  let component: LogsComponent;
  let fixture: ComponentFixture<LogsComponent>;
  let mockJobService: jasmine.SpyObj<JobService>;

  beforeEach(async () => {
    mockJobService = jasmine.createSpyObj('JobService', ['getLogs']);

    await TestBed.configureTestingModule({
      declarations: [ LogsComponent ],
      providers: [
        { provide: JobService, useValue: mockJobService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load logs on init', () => {
    const mockLogs = { logs: [], totalLogs: 0, totalPages: 1 };
    mockJobService.getLogs.and.returnValue(of(mockLogs));

    component.ngOnInit();

    expect(mockJobService.getLogs).toHaveBeenCalled();
    expect(component.logs).toEqual(mockLogs.logs);
  });

  it('should update pagination details when logs are loaded', () => {
    const mockLogs = { logs: [], totalLogs: 10, totalPages: 2 };
    mockJobService.getLogs.and.returnValue(of(mockLogs));

    component.loadLogs(2);

    expect(component.totalLogs).toBe(10);
    expect(component.totalPages).toBe(2);
    expect(component.currentPage).toBe(2);
  });
});
