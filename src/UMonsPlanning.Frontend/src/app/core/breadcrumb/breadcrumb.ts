import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface BreadcrumbItem {
  readonly label: string;
  readonly link?: string;
}

/** Visible breadcrumb trail for internal pages (e.g. "Accueil > Aide"). The structured-data
 * `BreadcrumbList` counterpart is a separate, later piece of work — this is the visible nav only. */
@Component({
  selector: 'app-breadcrumb',
  imports: [RouterLink],
  templateUrl: './breadcrumb.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Breadcrumb {
  readonly items = input.required<readonly BreadcrumbItem[]>();
}
