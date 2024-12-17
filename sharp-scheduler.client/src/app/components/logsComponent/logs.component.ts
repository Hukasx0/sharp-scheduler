import { Component, OnInit } from '@angular/core';
import { JobService } from '../../services/job.service'; // JobService to fetch logs related to jobs

@Component({
  selector: 'app-logs',
  templateUrl: './logs.component.html',
  styleUrls: ['./logs.component.css']
})
export class LogsComponent implements OnInit {
  logs: any[] = []; // List of logs fetched from the job service
  totalLogs: number = 0; // Total number of logs
  currentPage: number = 1; // Current page for pagination
  totalPages: number = 1; // Total number of pages for pagination

  constructor(private jobService: JobService) {}

  ngOnInit(): void {
    // Load logs when component initializes
    this.loadLogs();
  }

  // Load logs from the job service
  loadLogs(page: number = 1) {
    this.jobService.getLogs(page).subscribe(response => {
      this.logs = response.logs;
      this.totalLogs = response.totalLogs;
      this.totalPages = response.totalPages;
      this.currentPage = page;
    });
  }
}
