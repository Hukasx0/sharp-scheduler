import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let component: AppComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppComponent],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the app component', () => {
    expect(component).toBeTruthy();
  });

  it('should have the correct selector', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-root')).toBeTruthy();
  });

  it('should render the app component template', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    // Check if the template contains an element with a specific text or tag.
    // Adjust the query selector as per your actual template content.
    expect(compiled.querySelector('app-root')).toBeTruthy();
  });
});
