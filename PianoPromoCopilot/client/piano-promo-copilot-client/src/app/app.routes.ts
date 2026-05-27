export const routes = [
  { path: 'dashboard', loadComponent: () => import('./features/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'videos', loadComponent: () => import('./features/video-list.component').then(m => m.VideoListComponent) },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
];
