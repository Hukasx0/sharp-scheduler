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

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
