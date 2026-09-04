import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Breadcrumb } from './breadcrumb';

@Component({
  selector: 'app-breadcrumb-host',
  imports: [Breadcrumb],
  template: `<app-breadcrumb
    [items]="[{ label: 'Accueil', link: '/' }, { label: 'Aide' }]"
  />`,
})
class BreadcrumbHost {}

describe('Breadcrumb', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BreadcrumbHost],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should render a link for every item except the last, current one', () => {
    const fixture = TestBed.createComponent(BreadcrumbHost);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const link = compiled.querySelector('a');
    expect(link?.textContent?.trim()).toBe('Accueil');

    const current = compiled.querySelector('[aria-current="page"]');
    expect(current?.textContent?.trim()).toBe('Aide');
  });
});
