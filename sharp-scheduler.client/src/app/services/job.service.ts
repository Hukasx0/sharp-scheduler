import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Job } from '../interfaces/job';

@Injectable({
  providedIn: 'root'
})
export class JobService {
  private apiUrl = '/api/ScheduledJobs'; // API URL for job-related endpoints

  constructor(private http: HttpClient) { }

  // Fetch jobs with pagination support
  getJobs(page: number = 1, pageSize: number = 5): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }

  // Add a new job to the list
  addJob(job: Job): Observable<Job> {
    return this.http.post<Job>(this.apiUrl, job);
  }

  // Delete a job
  deleteJob(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Update the active status of a job
  updateJobStatus(id: number, active: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/active`, { active });
  }

  // Update job details
  updateJob(id: number, job: Job): Observable<Job> {
    return this.http.put<Job>(`${this.apiUrl}/${id}`, job);
  }

  // Get logs related to jobs
  getLogs(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/logs`);
  }
}
