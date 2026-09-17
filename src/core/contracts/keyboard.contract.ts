/**
 * Single Responsibility & Interface Segregation:
 * Pure keyboard input and key chords.
 * Strictly decoupled from mouse pointer and visual captures.
 */
export interface IKeyboardDriver {
  /** Type Unicode text into the currently focused control */
  typeText(text: string): Promise<boolean>;

  /** Press a key chord or shortcut (e.g. "Ctrl+S", "Alt+Tab", "Return") */
  pressKey(chord: string): Promise<boolean>;
}
