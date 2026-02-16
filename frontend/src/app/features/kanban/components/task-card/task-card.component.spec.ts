import { TestBed } from '@angular/core/testing';
import { TaskCardComponent } from './task-card.component';

describe('TaskCardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskCardComponent]
    }).compileComponents();
  });

  it('renders up to two labels and +N for remaining', () => {
    const fixture = TestBed.createComponent(TaskCardComponent);

    fixture.componentRef.setInput('task', {
      id: 'task-1',
      projectId: 'project-1',
      title: 'Task title',
      status: 'Todo',
      gitHubIssueNumber: 9,
      createdAt: new Date().toISOString(),
      completedAt: null
    });

    fixture.componentRef.setInput('labels', [
      { name: 'bug', color: 'd73a4a' },
      { name: 'frontend', color: '0e8a16' },
      { name: 'urgent', color: 'b60205' }
    ]);

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;

    expect(text).toContain('bug');
    expect(text).toContain('frontend');
    expect(text).toContain('+1');
  });
});
