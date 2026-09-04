import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Breadcrumb } from '../../core/breadcrumb/breadcrumb';
import type { GuideContent } from './guide-content';

/**
 * Shared page for every "how to add your UMONS schedule to X" destination page — the component is
 * the route's own leaf, so its content comes straight from `ActivatedRoute.snapshot.data['guide']`
 * (no leaf-walking needed, unlike SeoMetaService which sits above the whole route tree).
 */
@Component({
  selector: 'app-guide-page',
  imports: [RouterLink, Breadcrumb],
  templateUrl: './guide-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GuidePage {
  protected readonly guide = inject(ActivatedRoute).snapshot.data['guide'] as GuideContent;

  protected get breadcrumb() {
    return [{ label: 'Accueil', link: '/' }, { label: this.guide.breadcrumbLabel }];
  }
}
