import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Job } from '../interfaces/job';

@Injectable({
  providedIn: 'root'
})
export class JobService {
  private apiUrl = '/api/ScheduledJobs';

  constructor(private http: HttpClient) { }

  getJobs(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  addJob(job: Job): Observable<Job> {
    return this.http.post<Job>(this.apiUrl, job);
  }

  deleteJob(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  updateJobStatus(id: number, active: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/active`, { active });
  }

  updateJob(id: number, job: Job): Observable<Job> {
    return this.http.put<Job>(`${this.apiUrl}/${id}`, job);
  }
}
