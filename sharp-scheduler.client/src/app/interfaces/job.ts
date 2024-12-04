export interface Job {
    id: number;
    name: string;
    command: string;
    cronExpression: string;
    createdAt: string;
    lastExecution: string;
    isActive: boolean;
}
  