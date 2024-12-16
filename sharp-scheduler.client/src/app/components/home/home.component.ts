import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service'; // Auth service to manage user login and logout
import { Router } from '@angular/router'; // Router service for navigation
import { JobService } from '../../services/job.service'; // Job service to handle CRUD operations for jobs
import { FormGroup, FormControl, Validators } from '@angular/forms'; // Angular Reactive Forms for form validation
import { Job } from '../../interfaces/job'; // Job interface representing a job object

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  user = this.authService.currentUser; // Current logged-in user
  jobs: Job[] = []; // List of jobs fetched from the job service
  jobForm!: FormGroup; // Reactive form for adding or editing a job
  editingJob: Job | null = null; // Track if a job is being edited
  currentPage: number = 1; // Current page for pagination
  totalPages: number = 1; // Total number of pages for pagination

  constructor(public authService: AuthService, private router: Router, private jobService: JobService) { }

  ngOnInit(): void {
    // Initialize the form controls with validation
    this.jobForm = new FormGroup({
      name: new FormControl('', Validators.required),
      command: new FormControl('', Validators.required),
      cronExpression: new FormControl('', [Validators.required]),
      isActive: new FormControl(true)
    });

    // Load jobs when component initializes
    this.loadJobs();
  }

  // Load jobs from the job service
  loadJobs(page: number = this.currentPage) {
    this.jobService.getJobs(page).subscribe(response => {
      this.jobs = response.jobs;
      this.totalPages = response.totalPages;
      this.currentPage = response.currentPage;
    });
  }

  // Add a new job to the list
  addJob() {
    if (this.jobForm.valid) {
      const newJob = this.jobForm.value;
      this.jobService.addJob(newJob).subscribe(response => {
        this.jobs.push(response);
        this.jobForm.reset({ isActive: true });
      });
    }
  }

  // Delete a job from the list
  deleteJob(id: number) {
    this.jobService.deleteJob(id).subscribe(() => {
      this.jobs = this.jobs.filter(job => job.id !== id);
    });
  }

  // Toggle the status of a job (active/inactive)
  toggleActive(job: Job) {
    const updatedStatus = !job.isActive;
    this.jobService.updateJobStatus(job.id, updatedStatus).subscribe(() => {
      job.isActive = updatedStatus;
    });
  }

  // Start editing a job
  editJob(job: Job) {
    this.editingJob = job;
    this.jobForm.setValue({
      name: job.name,
      command: job.command,
      cronExpression: job.cronExpression,
      isActive: job.isActive
    });
  }

  // Update an existing job
  updateJob() {
    if (this.jobForm.valid && this.editingJob) {
      const updatedJob = { ...this.editingJob, ...this.jobForm.value };

      this.jobService.updateJob(updatedJob.id, updatedJob).subscribe(response => {
        this.loadJobs();
        this.cancelEdit();
      }, error => {
        console.error('Error updating job', error);
      });
    }
  }

  // Cancel the edit operation
  cancelEdit() {
    this.editingJob = null;
    this.jobForm.reset({ isActive: true });
  }

  // Navigate to the next page of jobs
  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.loadJobs(this.currentPage + 1);
    }
  }

  // Navigate to the previous page of jobs
  previousPage() {
    if (this.currentPage > 1) {
      this.loadJobs(this.currentPage - 1);
    }
  }

  // Refresh the job list
  refreshJobs() {
    this.cancelEdit();
    this.loadJobs();
  }

  // Log out the user
  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // Navigate to the logs page
  goToLogs() {
    this.router.navigate(['/logs']);
  }

  // Export the jobs to a cron file
  exportCronFile() {
    this.jobService.exportJobsAsCronFile().subscribe((response: Blob) => {
      const url = window.URL.createObjectURL(response);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'scheduled_jobs.cron';
      a.click();
      window.URL.revokeObjectURL(url);
    });
  }

  // Import jobs from a cron file
  importCronFile(event: any) {
    const file = event.target.files[0];
    if (file) {
      const formData = new FormData();
      formData.append('file', file);
      this.jobService.importJobsFromCronFile(formData).subscribe(() => {
        this.refreshJobs();
      }, error => {
        console.error('Error importing jobs', error);
      });
    }
  }
}
