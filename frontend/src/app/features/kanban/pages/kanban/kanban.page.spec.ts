import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Store } from '@ngrx/store';
import { vi } from 'vitest';
import { KanbanPage } from './kanban.page';
import { KanbanApiService } from '../../data/kanban-api.service';
import { SignalrService } from '../../../../core/signalr/signalr.service';
import { UiService } from '../../../../core/ui/ui.service';
import { TaskDto } from '../../models/kanban.models';

describe('KanbanPage', () => {
  let callIndex = 0;

  const detailsData = {
    taskId: 'task-1',
    issueNumber: 100,
    title: 'Issue title',
    description: 'Issue description',
    state: 'open',
    stateReason: null,
    author: null,
    assignees: [],
    labels: [
      { name: 'bug', color: 'd73a4a' },
      { name: 'frontend', color: '0e8a16' },
      { name: 'urgent', color: 'b60205' }
    ],
    commentsCount: 1,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    url: 'https://github.com/test/repo/issues/100'
  };

  const commentsData = [
    {
      id: 1,
      body: 'comment',
      author: null,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      url: null
    }
  ];

  const kanbanApiMock = {
    getIssueDetails: vi.fn().mockReturnValue(of({ success: true, data: detailsData, message: '' })),
    getIssueComments: vi.fn().mockReturnValue(of({ success: true, data: commentsData, message: '' }))
  };

  const signalrMock = {
    startConnection: vi.fn().mockReturnValue(Promise.resolve()),
    onTaskUpdated: vi.fn(),
    offTaskUpdated: vi.fn()
  };

  const uiMock = {
    showError: vi.fn()
  };

  const storeMock = {
    dispatch: vi.fn(),
    selectSignal: vi.fn(() => {
      const values = [
        signal([]),
        signal<string | null>(null),
        signal([]),
        signal([]),
        signal([]),
        signal(false)
      ];

      const next = values[callIndex] ?? signal(null);
      callIndex += 1;
      return next;
    })
  };

  beforeEach(async () => {
    callIndex = 0;
    kanbanApiMock.getIssueDetails.mockClear();
    kanbanApiMock.getIssueComments.mockClear();

    await TestBed.configureTestingModule({
      imports: [KanbanPage],
      providers: [
        { provide: Store, useValue: storeMock },
        { provide: KanbanApiService, useValue: kanbanApiMock },
        { provide: SignalrService, useValue: signalrMock },
        { provide: UiService, useValue: uiMock }
      ]
    }).compileComponents();
  });

  it('uses cached issue data when reopening same task', () => {
    const fixture = TestBed.createComponent(KanbanPage);
    const component = fixture.componentInstance;

    const task: TaskDto = {
      id: 'task-1',
      projectId: 'project-1',
      title: 'Task',
      status: 'Todo',
      gitHubIssueNumber: 100,
      createdAt: new Date().toISOString(),
      completedAt: null
    };

    component.openIssueDetails(task);
    component.closeIssueDetails();
    component.openIssueDetails(task);

    expect(kanbanApiMock.getIssueDetails).toHaveBeenCalledTimes(1);
    expect(kanbanApiMock.getIssueComments).toHaveBeenCalledTimes(1);
  });

  it('invalidates cache on status change and refetches details', () => {
    const fixture = TestBed.createComponent(KanbanPage);
    const component = fixture.componentInstance;

    const task: TaskDto = {
      id: 'task-2',
      projectId: 'project-1',
      title: 'Task 2',
      status: 'InProgress',
      gitHubIssueNumber: 101,
      createdAt: new Date().toISOString(),
      completedAt: null
    };

    component.openIssueDetails(task);

    component.drop(
      {
        item: { data: task }
      } as any,
      'Done'
    );

    component.openIssueDetails(task);

    expect(kanbanApiMock.getIssueDetails).toHaveBeenCalledTimes(2);
  });
});
