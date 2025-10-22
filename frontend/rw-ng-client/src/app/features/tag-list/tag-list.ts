import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TagsService } from './tags.service';
import { map, catchError, of } from 'rxjs';

@Component({
  selector: 'app-tag-list',
  imports: [],
  templateUrl: './tag-list.html',
  styleUrl: './tag-list.scss'
})
export class TagList {
  private tagsService = inject(TagsService);

  tags = toSignal(
    this.tagsService.getTags().pipe(
      map(response => response.tags),
      catchError((error) => {
        console.error('Error loading tags:', error);
        return of([]);
      })
    ),
    { initialValue: [] as string[] }
  );
}
