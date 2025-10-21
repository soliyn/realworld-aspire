import { Component } from '@angular/core';
import { ArticleListItem } from '../article-list-item/article-list-item';

@Component({
  selector: 'app-article-list',
  imports: [ArticleListItem],
  templateUrl: './article-list.html',
  styleUrl: './article-list.scss',
})
export class ArticleList {}
