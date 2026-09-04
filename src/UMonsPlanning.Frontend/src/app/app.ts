import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { LegalDisclaimerDialog } from './features/legal-disclaimer/legal-disclaimer-dialog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, NgOptimizedImage, LegalDisclaimerDialog],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
