import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Breadcrumb } from '../../core/breadcrumb/breadcrumb';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, Breadcrumb],
  templateUrl: './not-found-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {
  protected readonly breadcrumb = [{ label: 'Accueil', link: '/' }, { label: 'Page introuvable' }];
}
