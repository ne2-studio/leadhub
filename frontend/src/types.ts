// Example domain entity demonstrating the class-per-entity convention.
// Replace with the real domain model(s) for this project.
export class Task {
  id: string;
  title: string;
  createdAt: string;

  constructor(data: {
    id: string;
    title: string;
    createdAt: string;
  }) {
    this.id = data.id;
    this.title = data.title;
    this.createdAt = data.createdAt;
  }
}
