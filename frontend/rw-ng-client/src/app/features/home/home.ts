import { Component } from '@angular/core';
import { TagList } from '../tag-list/tag-list';
import { ArticleList } from '../articles/article-list/article-list';

@Component({
  selector: 'app-home',
  imports: [TagList, ArticleList],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {}
