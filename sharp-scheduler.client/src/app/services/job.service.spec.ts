import { TestBed } from '@angular/core/testing';
import { JobService } from './job.service';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Job } from '../interfaces/job';

describe('JobService', () => {
  let service: JobService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [JobService]
    });

    service = TestBed.inject(JobService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should fetch jobs with pagination', () => {
    const mockJobs = { jobs: [], totalJobs: 10, totalPages: 2 };
    service.getJobs(1, 5).subscribe((response) => {
      expect(response.jobs).toEqual([]);
    });

    const req = httpMock.expectOne('/api/ScheduledJobs?page=1&pageSize=5');
    req.flush(mockJobs);
  });

  it('should add a new job', () => {
    const mockJob: Job = {
      id: 1,
      name: 'New Job',
      command: 'echo hello',
      cronExpression: '0 0 * * *',
      createdAt: new Date().toISOString(),
      lastExecution: new Date().toISOString(),
      isActive: true
    };

    service.addJob(mockJob).subscribe((job) => {
      expect(job).toEqual(mockJob);
    });

    const req = httpMock.expectOne('/api/ScheduledJobs');
    req.flush(mockJob);
  });

  it('should delete a job', () => {
    service.deleteJob(1).subscribe();

    const req = httpMock.expectOne('/api/ScheduledJobs/1');
    req.flush({});
  });

  afterEach(() => {
    // Ensuring no outstanding HTTP requests are pending
    httpMock.verify();
  });
});
