import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { JobService } from '../../services/job.service';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Job } from '../../interfaces/job';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  user = this.authService.currentUser;
  jobs: Job[] = [];
  jobForm!: FormGroup;
  editingJob: Job | null = null;

  constructor(public authService: AuthService, private router: Router, private jobService: JobService) { }

  ngOnInit(): void {
    this.jobForm = new FormGroup({
      name: new FormControl('', Validators.required),
      command: new FormControl('', Validators.required),
      cronExpression: new FormControl('', [Validators.required]),
      isActive: new FormControl(true)
    });

    this.loadJobs();
  }

  loadJobs() {
    this.jobService.getJobs().subscribe(response => {
      this.jobs = response.jobs;
    });
  }

  addJob() {
    if (this.jobForm.valid) {
      const newJob = this.jobForm.value;
      this.jobService.addJob(newJob).subscribe(response => {
        this.jobs.push(response);
        this.jobForm.reset({ isActive: true });
      });
    }
  }

  deleteJob(id: number) {
    this.jobService.deleteJob(id).subscribe(() => {
      this.jobs = this.jobs.filter(job => job.id !== id);
    });
  }

  toggleActive(job: Job) {
    const updatedStatus = !job.isActive;
    this.jobService.updateJobStatus(job.id, updatedStatus).subscribe(() => {
      job.isActive = updatedStatus;
    });
  }

  editJob(job: Job) {
    this.editingJob = job;
    this.jobForm.setValue({
      name: job.name,
      command: job.command,
      cronExpression: job.cronExpression,
      isActive: job.isActive
    });
  }

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

  cancelEdit() {
    this.editingJob = null;
    this.jobForm.reset({ isActive: true });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  goToLogs() {
    this.router.navigate(['/logs']);
  }
}
