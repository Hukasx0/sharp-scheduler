import { Component, OnInit } from '@angular/core';
import { JobService } from '../../services/job.service';

@Component({
  selector: 'app-logs',
  templateUrl: './logs.component.html',
  styleUrl: './logs.component.css'
})
export class LogsComponent implements OnInit {
  logs: any[] = [];
  totalLogs: number = 0;
  currentPage: number = 1;
  totalPages: number = 1;

  constructor(private jobService: JobService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(page: number = 1) {
    this.jobService.getLogs().subscribe(response => {
      this.logs = response.logs;
      this.totalLogs = response.totalLogs;
      this.totalPages = response.totalPages;
      this.currentPage = page;
    });
  }
}
